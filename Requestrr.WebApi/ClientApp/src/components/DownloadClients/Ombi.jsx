import { useEffect, useState } from "react";
import { Oval } from 'react-loader-spinner'
import { useDispatch } from 'react-redux';
import { Alert } from "reactstrap";
import { testOmbiSettings as testSettings } from "../../store/actions/MovieClientsActions"
import ValidatedTextbox from "../Inputs/ValidatedTextbox"
import Textbox from "../Inputs/Textbox"
import Dropdown from "../Inputs/Dropdown"

import {
  FormGroup,
  Input,
  Row,
  Col
} from "reactstrap";
import { unstable_batchedUpdates } from "react-dom";


function Ombi(props) {
  const [ombiType] = useState(props.type);
  const [isTestingSettings, setIsTestingSettings] = useState(false);
  const [testSettingsRequested, setTestSettingsRequested] = useState(false);
  const [testSettingsSuccess, setTestSettingsSuccess] = useState(false);
  const [testSettingsError, setTestSettingsError] = useState("");
  const [hostname, setHostname] = useState("");
  const [isHostnameValid, setIsHostnameValid] = useState(false);
  const [port, setPort] = useState("5000");
  const [isPortValid, setIsPortValid] = useState(false);
  const [apiKey, setApiKey] = useState("");
  const [isApiKeyValid, setIsApiKeyValid] = useState(false);
  const [apiUsername, setApiUsername] = useState("");
  const [useSSL, setUseSSL] = useState("");
  const [useIssue, setUseIssue] = useState("");
  const [apiVersion, setApiVersion] = useState("");
  const [baseUrl, setBaseUrl] = useState("");

  const dispatch = useDispatch();


  useEffect(() => {
    updateStateFromProps(props);
  }, []);


  useEffect(() => {
    onValueChange();
  }, [useSSL, apiVersion, apiKey, port, baseUrl, apiUsername, useIssue]);


  useEffect(() => {
    onValidate();
  }, [isApiKeyValid, isHostnameValid, isPortValid]);



  const validateNonEmptyString = (value) => {
    return /\S/.test(value);
  };

  const validatePort = (value) => {
    return /^([0-9]{1,4}|[1-5][0-9]{4}|6[0-4][0-9]{3}|65[0-4][0-9]{2}|655[0-2][0-9]|6553[0-5])$/.test(value);
  };



  const updateStateFromProps = (props) => {
    setIsTestingSettings(false);
    setTestSettingsRequested(false);
    setTestSettingsSuccess(false);
    setTestSettingsError("");
    setHostname(props.settings.hostname);
    setIsHostnameValid(false);
    setPort(props.settings.port);
    setIsPortValid(false);
    setApiKey(props.settings.apiKey);
    setIsApiKeyValid(false);
    setApiUsername(props.settings.apiUsername);
    setBaseUrl(props.settings.baseUrl);
    setUseSSL(props.settings.useSSL);
    setApiVersion(props.settings.version);
    if (ombiType === "movie")
      setUseIssue(props.settings.useMovieIssue);
    else if (ombiType === "tv")
      setUseIssue(props.settings.useTVIssue);
  };


  const onUseSSLChanged = (event) => {
    setUseSSL(!useSSL);
  };


  const onUseIssueChanged = (event) => {
    setUseIssue(!useIssue);
  };


  const onTestSettings = (e) => {
    e.preventDefault();

    if (!isTestingSettings
      && isHostnameValid
      && isPortValid
      && isApiKeyValid) {
      setIsTestingSettings(true);

      dispatch(testSettings({
        hostname: hostname,
        baseUrl: baseUrl,
        port: port,
        apiKey: apiKey,
        useSSL: useSSL,
        version: apiVersion,
      }))
        .then(data => {
          setIsTestingSettings(false);

          if (data.ok) {
            setTestSettingsRequested(true);
            setTestSettingsError("");
            setTestSettingsSuccess(true);
          } else {
            let error = "An unknown error occurred while testing the settings";

            if (typeof (data.error) === "string")
              error = data.error;

            setTestSettingsRequested(true);
            setTestSettingsError(error);
            setTestSettingsSuccess(false);
          }
        });
    }
  };


  const onValidate = () => {
    props.onValidate(isApiKeyValid && isHostnameValid && isPortValid);
  };

  const onValueChange = () => {
    let settings = {
      hostname: hostname,
      baseUrl: baseUrl,
      port: port,
      apiKey: apiKey,
      apiUsername: apiUsername,
      useSSL: useSSL,
      version: apiVersion
    }

    if (ombiType === "movie")
      settings.useMovieIssue = useIssue;
    else if (ombiType === "tv")
      settings.useTVIssue = useIssue;

    props.onChange(settings);

    onValidate();
  };





  return (
    <>
      <div>
        <h6 className="heading-small text-muted mb-4">
          Ombi Connection Settings
        </h6>
      </div>
      <div className="pl-lg-4">
        <Row>
          <Col lg="6">
            <Dropdown
              name="API"
              value={apiVersion}
              items={[{ name: "Version 3-4", value: "3" }]}
              onChange={newApiVersion => setApiVersion(newApiVersion)} />
          </Col>
          <Col lg="6">
            <ValidatedTextbox
              name="API Key"
              placeholder="Enter api key"
              alertClassName="mt-3"
              errorMessage="api key is required."
              isSubmitted={props.isSubmitted}
              value={apiKey}
              validation={validateNonEmptyString}
              onChange={newApiKey => setApiKey(newApiKey)}
              onValidate={isValid => setIsApiKeyValid(isValid)} />
          </Col>
        </Row>
        <Row>
          <Col lg="6">
            <ValidatedTextbox
              name="Host or IP"
              placeholder="Enter host or ip"
              alertClassName="mt-3 mb-0"
              errorMessage="Hostname is required."
              isSubmitted={props.isSubmitted}
              value={hostname}
              validation={validateNonEmptyString}
              onChange={newHostname => setHostname(newHostname)}
              onValidate={isValid => setIsHostnameValid(isValid)} />
          </Col>
          <Col lg="6">
            <ValidatedTextbox
              name="Port"
              placeholder="Enter port"
              alertClassName="mt-3 mb-0"
              errorMessage="Please enter a valid port."
              isSubmitted={props.isSubmitted}
              value={port}
              validation={validatePort}
              onChange={newPort => setPort(newPort)}
              onValidate={isValid => setIsPortValid(isValid)} />
          </Col>
        </Row>
        <Row>
          <Col lg="6">
            <Textbox
              name="Base Url"
              placeholder="Enter base url configured in Ombi, leave empty if none configured."
              value={baseUrl}
              onChange={newBaseUrl => setBaseUrl(newBaseUrl)} />
          </Col>
          <Col lg="6">
            <Textbox
              name="Default Ombi Username"
              placeholder="Enter api username (Optional)"
              value={apiUsername}
              onChange={newApiUsername => setApiUsername(newApiUsername)} />
          </Col>
        </Row>
        <Row>
          <Col lg="6">
            <FormGroup className="custom-control custom-control-alternative custom-checkbox mb-3">
              <Input
                className="custom-control-input"
                id="useSSL"
                type="checkbox"
                onChange={onUseSSLChanged}
                checked={useSSL}
              />
              <label
                className="custom-control-label"
                htmlFor="useSSL">
                <span className="text-muted">Use SSL</span>
              </label>
            </FormGroup>
          </Col>
          <Col lg="6">
            <a href="https://docs.ombi.app/" target="_blank" rel="noreferrer">View Ombi documentation</a>
          </Col>
        </Row>
        <Row>
          <Col>
            <FormGroup className="mt-4">
              {
                testSettingsRequested && !isTestingSettings ?
                  !testSettingsSuccess ? (
                    <Alert className="text-center" color="danger">
                      <strong>{testSettingsError}</strong>
                    </Alert>)
                    : <Alert className="text-center" color="success">
                      <strong>The specified settings are valid.</strong>
                    </Alert>
                  : null
              }
            </FormGroup>
          </Col>
        </Row>
        <Row>
          <Col>
            <FormGroup className="text-right">
              <button onClick={onTestSettings} disabled={!isHostnameValid || !isPortValid || !isApiKeyValid} className="btn btn-icon btn-3 btn-default" type="button" >
                <span className="btn-inner--icon">
                  {
                    isTestingSettings ? (
                      <Oval
                        wrapperClass="loader"
                        type="Oval"
                        color="#11cdef"
                        height={19}
                        width={19}
                      />)
                      : (<i className="fas fa-cogs"></i>)
                  }</span>
                <span className="btn-inner--text">Test Settings</span>
              </button>
            </FormGroup>
          </Col>
        </Row>
      </div>
      <hr className="my-4" />
      <h6 className="heading-small text-muted">
        Enable Issues
      </h6>
      <div>
        <Col lg="6">
          <FormGroup className="custom-control custom-control-alternative custom-checkbox mb-3">
            <Input
              className="custom-control-input"
              id="useIssue"
              type="checkbox"
              onChange={onUseIssueChanged}
              checked={useIssue}
            />
            <label
              className="custom-control-label"
              htmlFor="useIssue">
              <span className="text-muted">Use Issues</span>
            </label>
          </FormGroup>
        </Col>
      </div>
      <hr className="my-4" />
    </>
  );
}


export default Ombi;